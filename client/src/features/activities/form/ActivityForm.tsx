import { Box, Button, Paper, TextField, Typography } from "@mui/material";
import type { FormEvent } from "react";

type Props = {
    activity?: Activity 
    closeForm: () => void
    submitForm: (activity: Activity) => void
}

export default function ActivityForm({ activity, closeForm, submitForm }: Props) {

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);

    const data: {[key: string]: FormDataEntryValue} = {};
    formData.forEach((value, key) => {
        data[key] = value;
    });

    // 假如 prop 有 activity 的話，表示這是 edit，因此再補上 id
    if(activity) data.id = activity.id;

    // 強迫轉型的作法，避免 typescript warning
    submitForm(data as unknown as Activity);
  };

  return (
    <Paper sx={{ borderRadius: 3, padding: 3 }}>
        <Typography variant="h5" gutterBottom color="primary">
            Create activity
        </Typography>
        <Box component="form" onSubmit={handleSubmit} display="flex" flexDirection="column" gap={3}>
            <TextField name="title" label="Title" defaultValue={activity?.title} />
            <TextField name="description" label="Description" multiline rows={3} defaultValue={activity?.description} />
            <TextField name="category" label="Category" defaultValue={activity?.category} />
            <TextField name="date" label="Date" type="date" defaultValue={activity?.date} />
            <TextField name="city" label="City" defaultValue={activity?.city} />
            <TextField name="venue" label="Venue" defaultValue={activity?.venue} />
            <Box display="flex" justifyContent="end" gap={3}>
                <Button onClick={closeForm} color="inherit">Cancel</Button>
                <Button type="submit" color="success" variant="contained">Submit</Button>
            </Box>
        </Box>
    </Paper>
  )
}